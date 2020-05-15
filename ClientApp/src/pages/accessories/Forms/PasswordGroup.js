import React, { useState } from "react";
import { Modal } from "react-bootstrap";
import validator from "../../../utils/validator";
import LocalizedStrings from "react-localization";
import Password from "react-type-password";

const text = new LocalizedStrings({
    en:{
        title:"Change password",
        label: "Password",
        newholder: "New password",
        confholder: "Confirm password",
        change: "Change",
        confirm: "Confirm",
        clear: "Clear",
    },
    ru:{
        title:"Изменить пароль",
        label: "Пароль",
        newholder: "Новый пароль",
        confholder: "Подтвердите Пароль",
        change: "Изменить",
        confirm: "Подтвердить",
        clear: "Очистить",
    }
})

export function PasswordGroup(props) {
    const [showModal, setShowModal] = useState(false);
    const [newpassword, setNewPassword] = useState(props.newpassword);
    const [confirm, setConfirm] = useState(props.newpassword);
    const [errors, setErrors] = useState(() => ({
        newpassword: validator.password(props.newpassword, props.lang),
        confirm: validator.confirm(props.newpassword, props.newpassword, props.lang)
    }));
    const show = () => {
        setNewPassword(props.newpassword);
        setConfirm(props.newpassword);
        setErrors({
            newpassword: validator.password(props.newpassword, props.lang),
            confirm: validator.confirm(props.newpassword, props.newpassword, props.lang)
        });
    }
    const hide = () => {
        setShowModal(false)
    }
    const passwordChanged = val => {
        setNewPassword(val);
        if (confirm) {
            setErrors({
                newpassword: validator.password(val, props.lang),
                confirm: validator.confirm(val, confirm, props.lang)
            });
        } else setErrors({ ...errors, newpassword: validator.password(val, props.lang) });
    }
    const confirmChanged = val => {
        setConfirm(val);
        setErrors({ ...errors, confirm: validator.confirm(newpassword, val, props.lang) });
    }
    const clearInputs = () => {
        setNewPassword("");
        setConfirm("");
        setErrors({
            newpassword: validator.password("", props.lang),
            confirm: validator.confirm("", "", props.lang)
        });
    }
    const confirmChange = () => {
        if((!errors.newpassword && !errors.confirm) || (!newpassword && !confirm)){
            props.onChange(newpassword);
            setShowModal(false);
        }
    }
    text.setLanguage(props.lang);
    return <>
        <div className="form-group">
            <div className="row pb-4">
                <div className="col-3">
                    <label className="h5">{text.label}</label>
                </div>
                <div className="col">
                    <div className="input-group">
                        <div className="input-group-prepend">
                            <span className="input-group-text"><strong className="invisible">&ndash;</strong></span>
                        </div>
                        <button onClick={() => setShowModal(true)} className="btn btn-outline-secondary">{text.change}</button>
                    </div>
                </div>
            </div>
        </div>
        <Modal id="password_modal" show={showModal} onHide={hide} onShow={show} >
            <Modal.Header closeButton>
                <Modal.Title>{text.title}</Modal.Title>
            </Modal.Header>
            <Modal.Body>
                <div className="form-group">
                    <Password type={props.type} className="form-control" placeholder={text.newholder}
                        value={newpassword} onChange={passwordChanged}/>

                    <p className="text-danger">{errors.newpassword}</p>
                </div>
                <div className="form-group">
                    <Password className="form-control" placeholder={text.confholder} value={confirm}
                        onChange={confirmChanged} />
                    <p className="text-danger">{errors.confirm}</p>
                </div>
            </Modal.Body>
            <Modal.Footer>
                <button className="btn btn-outline-secondary" onClick={clearInputs}>{text.clear}</button>
                <button className="btn btn-info" onClick={confirmChange}>{text.confirm}</button>
            </Modal.Footer>
        </Modal>
    </>
}